package com.ordermanager.api.repository;

import com.ordermanager.api.model.CustomerOrder;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface OrderRepository extends JpaRepository<CustomerOrder, Integer> {

    List<CustomerOrder> findAllByOrderByOrderDateDesc();
}
